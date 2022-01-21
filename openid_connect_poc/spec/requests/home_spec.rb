# frozen_string_literal: true

require 'rails_helper'

RSpec.describe 'Homes', type: :request do
  describe 'GET /index' do
    subject { get '/' }

    it { is_expected.to eq(200) }
  end
end
